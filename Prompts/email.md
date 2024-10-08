Please use the following info to write a function that sends an email to each of the invited users:
    Outgoing server name: mailout.one.com
    Port and encryption:
    - 587 with STARTTLS (recommended)
    - 465 with SSL/TLS
    - 25 with STARTTLS or none
    Authentication: 
        username: Chore@forstberg.com
        password: verysecretpassword

        Please create the function in a class located in the "Integrations" folder. Also create a secrets file where the password is stored. This secret file will not be checked in to github.

        The Class should be named SmtpEmailSender and the method should be named SendInvitationEmail.

        The method should take in an InvitationDto and send an email to the invitee with a link to the chores app. The email should always be sent from "Chores <Chore@forstberg.com>"

        The subject of the email should be "You are invited to join a family in the Chores app"

        The body of the email should contain the following: 

        Hi there! You've been invited to join a family in the Chores app.

        Please click the link below to sign up and start managing your chores and earning some extra cash!

        [Sign up now](https://chores.forstberg.com/signup)

        If you have any questions, please contact [the creator of the family].
        
        Best regards,
        The Chores team
