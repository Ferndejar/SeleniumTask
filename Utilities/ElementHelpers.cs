using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using Serilog;

namespace SeleniumTask.Utilities
{
    /// <summary>
    /// Helper utilities for interacting with elements in a robust way across browsers and framework bindings (React, etc.).
    /// </summary>
    public static class ElementHelpers
    {
        /// <summary>
        /// Clears the input located by the given locator reliably:
        /// - re-finds the element (avoids stale references),
        /// - sets the value using the native setter so framework value trackers (React) are updated,
        /// - updates React's internal _valueTracker where present,
        /// - dispatches InputEvent('input') and change events,
        /// - attempts a user-like keyboard clear (Ctrl+A Delete),
        /// - repeats the JS clear after a short delay to beat async autofill,
        /// - waits until the element value is empty (re-finds during wait to avoid stale).
        /// </summary>
        public static void ClearAndNotify(IWebDriver driver, By locator, TimeSpan? timeout = null)
        {
            if (driver == null) throw new ArgumentNullException(nameof(driver));
            if (locator == null) throw new ArgumentNullException(nameof(locator));

            var js = (IJavaScriptExecutor)driver;

            IWebElement element;
            try
            {
                element = driver.FindElement(locator);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ClearAndNotify: failed to find element {Locator}", locator);
                throw;
            }

            // Aggressive JS clear that:
            // - uses native setter, clears defaultValue,
            // - updates React's _valueTracker if present,
            // - dispatches InputEvent + input/change/composition events,
            // - removes value/name/autocomplete attributes,
            // - runs twice (immediate + delayed) to beat async autofill
            var clearScript = @"
(function(el){
  try {
    var doClear = function() {
      try {
        // native setter
        var nativeSetter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set;
        if (nativeSetter) nativeSetter.call(el, '');
      } catch(e){}

      try { el.defaultValue = ''; } catch(e){}
      try { el.removeAttribute('value'); } catch(e){}
      try { el.setAttribute('autocomplete', 'off'); } catch(e){}
      try { el.removeAttribute('name'); } catch(e){} // avoid name-based autofill heuristics

      // Try to update React's tracker if present
      try {
        if (el._valueTracker && typeof el._valueTracker.setValue === 'function') {
          try { el._valueTracker.setValue(''); } catch(e){}
        } else {
          // Some React versions store tracker on prototype internals; attempt a generic approach
          try {
            var prop = Object.getOwnPropertyNames(el).filter(function(n){ return n.indexOf('_valueTracker')>=0; })[0];
            if (prop && el[prop] && typeof el[prop].setValue === 'function') el[prop].setValue('');
          } catch(e){}
        }
      } catch(e) {}

      // dispatch various events to ensure framework onChange/onInput handlers run
      try { 
        if (typeof InputEvent === 'function') {
          try { el.dispatchEvent(new InputEvent('input', {bubbles:true, cancelable:true, data: ''})); } catch(e){}
        } else {
          try { el.dispatchEvent(new Event('input', {bubbles:true, cancelable:true})); } catch(e){}
        }
      } catch(e){}
      try { el.dispatchEvent(new Event('change', {bubbles:true, cancelable:true})); } catch(e){}
      try { el.dispatchEvent(new Event('compositionend', {bubbles:true, cancelable:true})); } catch(e){}
      try { el.blur(); } catch(e){}
      try { el.focus(); } catch(e){}
    };

    // Run now and schedule a short delayed repeat to beat async autofill/password-manager inserts
    doClear();
    setTimeout(doClear, 120);
    return true;
  } catch(e) {
    return false;
  }
})(arguments[0]);
";

            try
            {
                var res = js.ExecuteScript(clearScript, element);
                Log.Debug("ClearAndNotify: aggressive JS clear executed for {Locator}, result={Result}", locator, res);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "ClearAndNotify: aggressive JS clear failed for {Locator}", locator);
            }

            // Best-effort keyboard clear to mimic user actions (some handlers listen to keyboard)
            try
            {
                var actions = new Actions(driver);
                actions.MoveToElement(element).Click()
                       .KeyDown(Keys.Control).SendKeys("a").KeyUp(Keys.Control)
                       .SendKeys(Keys.Delete)
                       .Perform();
                Log.Debug("ClearAndNotify: keyboard clear attempted for {Locator}", locator);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "ClearAndNotify: keyboard clear failed for {Locator}", locator);
            }

            // Small pause to let the delayed JS clear run
            try { System.Threading.Thread.Sleep(180); } catch (Exception) { /* Ignore exception */ }

            // Wait until the element's value is empty (re-find inside wait to avoid stale references)
            var wait = new WebDriverWait(driver, timeout ?? TimeSpan.FromSeconds(3));
            try
            {
                wait.Until(d =>
                {
                    try
                    {
                        var current = d.FindElement(locator).GetAttribute("value") ?? string.Empty;
                        var isEmpty = string.IsNullOrEmpty(current);
                        if (isEmpty)
                        {
                            Log.Debug("ClearAndNotify: element {Locator} is empty now", locator);
                        }
                        else
                        {
                            Log.Debug("ClearAndNotify: element {Locator} still has value '{Value}'", locator, current);
                        }
                        return isEmpty;
                    }
                    catch
                    {
                        return false;
                    }
                });
            }
            catch (WebDriverTimeoutException ex)
            {
                Log.Warning(ex, "ClearAndNotify: timeout waiting for element {Locator} to become empty", locator);
                // proceed - callers may capture diagnostics or retry
            }
        }

        /// <summary>
        /// After the DOM value is cleared, try to notify common front-end frameworks (React) so component state is updated too.
        /// This attempts to call React's internal onChange handler by locating the React internal props/fiber and invoking the callback.
        /// Safe best-effort: it swallows errors and returns bool indicating whether invocation succeeded.
        /// </summary>
        public static bool NotifyFrameworkAboutClear(IWebDriver driver, By locator)
        {
            if (driver == null) throw new ArgumentNullException(nameof(driver));
            if (locator == null) throw new ArgumentNullException(nameof(locator));

            try
            {
                var js = (IJavaScriptExecutor)driver;
                var script = @"
(function(el){
  try{
    if(!el) return false;
    // Ensure DOM empty again (native setter)
    try {
      var nativeSetter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set;
      if (nativeSetter) nativeSetter.call(el, '');
    } catch(e){}
    // Dispatch the usual events
    try { el.dispatchEvent(new Event('input', {bubbles:true, cancelable:true})); } catch(e){}
    try { el.dispatchEvent(new Event('change', {bubbles:true, cancelable:true})); } catch(e){}

    // 1) Try to find React fiber/internal instance and call props.onChange
    var callOnChangeFromFiber = function(fiber){
      try {
        var props = fiber.memoizedProps || (fiber.return && fiber.return.memoizedProps) || null;
        if (props && typeof props.onChange === 'function') {
          try { props.onChange({ target: el, currentTarget: el, nativeEvent: {}, preventDefault: function(){}, stopPropagation:function(){}, isTrusted:true }); } catch(e){}
          return true;
        }
      } catch(e){}
      return false;
    };

    for(var k in el){
      try {
        if(k.indexOf('__reactFiber$')===0 || k.indexOf('__reactInternalInstance$')===0 || k.indexOf('__reactEventHandlers$')===0){
          var fiber = el[k];
          if(fiber && callOnChangeFromFiber(fiber)) return true;
        }
      } catch(e){}
    }

    // 2) Try the __reactProps* fallback (some bundlers)
    for(var p in el){
      try {
        if(p.indexOf('__reactProps')===0){
          var props = el[p];
          if(props && typeof props.onChange === 'function'){
            try { props.onChange({ target: el, currentTarget: el }); } catch(e) {}
            return true;
          }
        }
      } catch(e){}
    }

    // 3) Fallback: find nearest fiber and call stateNode.setState if possible (best-effort)
    try {
      for(var k2 in el){
        if(k2.indexOf('__reactFiber$')===0 || k2.indexOf('__reactInternalInstance$')===0){
          var f = el[k2];
          var node = f;
          while(node && !node.stateNode) node = node.return;
          if(node && node.stateNode && typeof node.stateNode.setState === 'function'){
            try { node.stateNode.setState(function(s){ return s; }); } catch(e){}
            return true;
          }
        }
      }
    } catch(e){}

    // 4) Last fallback: call element.onchange if present
    try { if(typeof el.onchange === 'function') el.onchange({ target: el }); } catch(e){}

    return true;
  } catch(e) {
    return false;
  }
})(arguments[0]);
";
                var element = driver.FindElement(locator);
                var res = js.ExecuteScript(script, element);
                Log.Debug("NotifyFrameworkAboutClear: result={Result} for {Locator}", res, locator);
                return res is bool b && b;
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "NotifyFrameworkAboutClear: exception for {Locator}", locator);
                return false;
            }
        }
    }
}