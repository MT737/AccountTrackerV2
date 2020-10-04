# AccountTrackerV2

### Overview
This is a simple ASP.Net Core web application making use of Entity Framework and Entity Framework Identity. I'm building and updating the application as a personal exercise. As such, it should not be utilized by anyone for the storage of sensitive and required data. WARNING! Data will not be persisted into perpetuity and the site is not SSL/TSL protected. 

### Immediate Tasks
1. ~~Add sorting to Accounts, Categories, and Vendors indexes.~~ 9/26/2020
2. ~~Add filtering to Accounts, Categories, and Vendors indexes.~~ 9/26/2020
3. ~~Add pagination to Accounts, Categories, and Vendors indexes.~~ 9/26/2020
4. ~~Correct issue in which entity editing validation failure messages are presented in green success textboxes.~~ 9/26/2020
5. ~~Update old HTML BootStrap formating (panels are now cards)~~ 9/27/20
6. Further test account balances for accuracy after multiple transactions

### Roadmap
1. Implement email varification and password recovery
2. Consider implementation of external log-in methods
3. Further refactoring (such as possibly Direct Injecting view models, comparing ViewData to ViewBag, Pushing DRY). Finally, review the possiblity of migrating all sorting, filtering, and pagination ViewBag items to related ViewModels.
4. Inform user to create an account if none are present. (Basically, provide the user with more friendly direction)
5. Determine if limiting category select list items that appear when creating a new transaction is necessary.
6. Determine if addtional validation measures are required as part of the transaction add and an edit post actions. 
7. Research improved method for calculating account balance.
8. Implement bulk DB update for Absorption method (both category and vendor). 
9. Look for a better way to set decimal scale for a property
10. Create dynamic BadRequest and NotFound pages/messages to provide further feedback to the user.
11. Remaining TODO: task list items
12. Additional Theme/Layout changes
13. Implement BootStrap custom validation features.
