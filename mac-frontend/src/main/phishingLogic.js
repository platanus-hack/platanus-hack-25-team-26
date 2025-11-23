class PhishingLogic {
  constructor() {
    this.state = 'capturing';
    this.isAlertVisible = false;
    this.currentAlert = null;
    this.originalAlertScore = null; // Score that triggered the alert
    this.lastWindowKey = null; // Track app + title for tab changes
  }

  processResponse(backendResponse, currentWindow = null) {
    const scoring = backendResponse.scoring || 0;
    
    // Create unique window key (app + title) to detect tab changes
    const currentWindowKey = currentWindow ? 
      `${currentWindow.app}|||${currentWindow.title}` : null;
    
    console.log(`[PhishingLogic] Processing: score=${scoring}, window="${currentWindow?.app}", alertVisible=${this.isAlertVisible}`);
    
    // RULE 1: Check if window changed (app OR tab)
    const windowChanged = this.lastWindowKey !== null && 
                         currentWindowKey !== null && 
                         this.lastWindowKey !== currentWindowKey;
    
    if (windowChanged) {
      console.log(`[PhishingLogic] Window/tab changed`);
      this.lastWindowKey = currentWindowKey;
      
      if (this.isAlertVisible) {
        console.log('[PhishingLogic] HIDE - window/tab changed, alert was visible');
        this.isAlertVisible = false;
        this.currentAlert = null;
        this.originalAlertScore = null;
        return { action: 'HIDE', alertData: null };
      }
      
      // No alert was showing - continue processing to check if new window needs alert
      console.log('[PhishingLogic] Window changed but no alert was showing, continue processing');
    } else {
      // No window change - update tracking
      this.lastWindowKey = currentWindowKey;
    }
    
    // RULE 2: Determine if we should show alert (score >= 3)
    const shouldShowAlert = scoring >= 3;
    
    // STATE 1: Need to show alert (risky but not showing)
    if (shouldShowAlert && !this.isAlertVisible) {
      console.log('[PhishingLogic] SHOW_NEW - score is risky and no alert visible');
      this.isAlertVisible = true;
      this.originalAlertScore = scoring; // Remember the score that triggered this alert
      
      this.currentAlert = {
        title: backendResponse.title || 'Security Alert',
        message: backendResponse.message || 'Suspicious content',
        details: backendResponse.details || 'Be careful',
        severity: backendResponse.severity || 'medium',
        scoring: scoring,
        timestamp: Date.now()
      };
      return { action: 'SHOW_NEW', alertData: this.currentAlert };
    }
    
    // STATE 2: Alert showing and still risky - check if we should update
    if (shouldShowAlert && this.isAlertVisible) {
      // Compare with ORIGINAL score that triggered the alert, not previous score
      const scoreDiff = Math.abs(scoring - this.originalAlertScore);
      
      console.log(`[PhishingLogic] Alert visible, score diff from original = ${scoreDiff} (original=${this.originalAlertScore}, current=${scoring})`);
      
      // Only update if score changed by 2+ from ORIGINAL
      if (scoreDiff >= 2) {
        console.log('[PhishingLogic] UPDATE - score changed significantly from original');
        this.originalAlertScore = scoring; // Update baseline for next comparisons
        
        this.currentAlert = {
          title: backendResponse.title || 'Security Alert',
          message: backendResponse.message || 'Suspicious content',
          details: backendResponse.details || 'Be careful',
          severity: backendResponse.severity || 'medium',
          scoring: scoring,
          timestamp: Date.now()
        };
        return { action: 'UPDATE', alertData: this.currentAlert };
      }
      
      console.log('[PhishingLogic] NOTHING - score within 2 points of original');
      return { action: 'NOTHING', alertData: null };
    }
    
    // STATE 3: Alert showing but no longer risky - hide it
    if (!shouldShowAlert && this.isAlertVisible) {
      console.log('[PhishingLogic] HIDE - score dropped below 3');
      this.isAlertVisible = false;
      this.currentAlert = null;
      this.originalAlertScore = null;
      return { action: 'HIDE', alertData: null };
    }
    
    // STATE 4: Not risky and no alert - do nothing
    console.log('[PhishingLogic] NOTHING - safe and no alert');
    return { action: 'NOTHING', alertData: null };
  }

  getState() {
    return this.state;
  }

  setState(newState) {
    this.state = newState;
  }

  getCurrentAlert() {
    return this.currentAlert;
  }

  reset() {
    this.state = 'idle';
    this.currentAlert = null;
    this.isAlertVisible = false;
    this.originalAlertScore = null;
    this.lastWindowKey = null;
  }
}

module.exports = PhishingLogic;
