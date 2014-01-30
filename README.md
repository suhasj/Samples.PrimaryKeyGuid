Samples.PrimaryKeyGuid
======================

Sample application taken from the nightly samples package. The primary key type is being changed from string to Guid
Currently the project references nightly packages which will be replaced with the offical ones after they are released

Steps to run project
- Clone project on local machine
- Open in VS and try to build
- The build fails because the Identity packages 2.0.0-beta-140129 may not be found. This is because they are being loaded from the nightly feed, 'http://myget.org/f/aspnetwebstacknightly' which might have different build number for that day
- To fix this, set up a private feed for nuget for http://www.myget.org/f/aspnetwebstacknightly. More information http://docs.nuget.org/docs/creating-packages/hosting-your-own-nuget-feeds 
- Right click on project and select 'Manage Nuget packages'. In the online tab, click on the above private feed
- Search for the Identity packages. Note down the version
- Change to this version for Identity packages in the packages.config file. Now build project which causes the packages to be downloaded
- Edit the Identity dll references for the project to point to the correct folders.
- This should build the solution with no errors
 
The project currrently has the primary key for the user set to Guid. To change it to int,
- Navigate to IdentityModel.cs and change the places where Guid is used to int. Remove the constructor in ApplicationUser to set the Id.
- Change the ApplicationDbContext and ApplicationUserManager references of Guid to int.
- Make corresponding changes in controller classes from Guid.Parse() to Int32.parse()

The EmailService class configures the method to send an email. Replace the credentials for the Gmail password and password with the ones for the app developer. This should use Gmail's smtp server to send the emails
