// Function to find the chat textarea, paste the prompt, and click submit.
// Accepts the prompt text as an argument.
function pasteAndSubmitPrompt(promptText) {
    try {
        // Find the primary textarea (adjust selector if needed for specific chat site)
        let ta = document.querySelector('textarea[tabindex="0"]'); 
        if (ta) {
            ta.value = promptText;
            // Trigger input event to make sure site recognizes the change
            ta.dispatchEvent(new Event('input', { bubbles: true }));

            // Find the submit button (heuristic: often near the textarea)
            // Adjust selector based on actual site structure (e.g., class name, data-testid)
            let btn = ta.closest('form')?.querySelector('button[type="submit"], button:not([disabled])[class*="send"], button:not([disabled])[data-testid*="send"]'); 
            if (!btn) { // Fallback if not in form or specific class
               btn = ta.parentElement?.querySelector('button:not([disabled])');
            }
            
            if (btn) {
                console.log('InjectAndSubmit: Found submit button:', btn);
                // Try to ensure the button is enabled before clicking
                // Some sites might disable it briefly after input
                setTimeout(() => {
                   try {
                       btn.disabled = false; 
                       btn.click();
                       console.log('InjectAndSubmit: Submit button clicked.');
                   } catch (clickError) {
                        console.error('InjectAndSubmit: Error clicking submit button:', clickError);
                   }
                }, 100); // Small delay might help
            } else {
                console.error('InjectAndSubmit: Submit button not found near textarea.');
            }
        } else {
            console.error('InjectAndSubmit: Textarea not found.');
        }
    } catch (e) {
        console.error('InjectAndSubmit Error:', e);
    }
}
