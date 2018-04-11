using Android.App;
using Android.Widget;
using Android.OS;
using Android.Support.V7.App;
using Android.Views;
using Android.Content.PM;
using Android.Runtime;
using Android.Annotation;
using Android.Animation;
using System.Collections.Generic;
using Java.Lang;
using Android.Views.InputMethods;
using static Android.App.LoaderManager;
using Android.Database;
using Android;
using Android.Support.Design.Widget;
using Android.Text;
using Android.Content;
using Android.Net;
using Android.Provider;

namespace XamarinAndroidDemo
{
    [Activity(Label = "XamarinAndroidDemo", MainLauncher = true)]
    public class MainActivity : AppCompatActivity, ILoaderCallbacks
    {
        //Id to identity READ_CONTACTS permission request.
        private static int REQUEST_READ_CONTACTS = 0;

        private static string[] DUMMY_CREDENTIALS = new string[]{
            "foo@example.com:hello", "bar@example.com:world"
        };

        private UserLoginTask mAuthTask = null;

        // UI references.
        private AutoCompleteTextView mEmailView;
        private EditText mPasswordView;
        private View mProgressView;
        private View mLoginFormView;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            mEmailView = (AutoCompleteTextView)FindViewById(Resource.Id.email);
            PopulateAutoComplete();

            mPasswordView = (EditText)FindViewById(Resource.Id.password);

            mPasswordView.EditorAction += (s, e) =>
            {
                if (e.ActionId == ImeAction.Done || e.ActionId == ImeAction.ImeNull)
                {
                    attemptLogin();
                    e.Handled = true;
                }
                e.Handled = false;
            };

            Button mEmailSignInButton = (Button)FindViewById(Resource.Id.email_sign_in_button);
            mEmailSignInButton.Click += (s, e) =>
            {
                attemptLogin();
            };

            mLoginFormView = FindViewById(Resource.Id.login_form);
            mProgressView = FindViewById(Resource.Id.login_progress);
        }

        private void PopulateAutoComplete()
        {
            if (!MayRequestContacts())
            {
                return;
            }

            LoaderManager.InitLoader(0, null, this);
        }

        private bool MayRequestContacts()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.M)
            {
                return true;
            }
            if (CheckSelfPermission(Manifest.Permission.ReadContacts) == Permission.Granted)
            {
                return true;
            }
            if (ShouldShowRequestPermissionRationale(Manifest.Permission.ReadContacts))
            {
                Snackbar.Make(mEmailView, Resource.String.permission_rationale, Snackbar.LengthIndefinite)
                        .SetAction(Android.Resource.String.Ok, (v) =>
                        {
                            RequestPermissions(new string[] { Manifest.Permission.ReadContacts }, REQUEST_READ_CONTACTS);
                        });
            }
            else
            {
                RequestPermissions(new string[]{Manifest.Permission.ReadContacts}, REQUEST_READ_CONTACTS);
            }
            return false;
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            if (requestCode == REQUEST_READ_CONTACTS)
            {
                if (grantResults.Length == 1 && grantResults[0] == Permission.Granted)
                {
                    PopulateAutoComplete();
                }
            }
        }

        private void attemptLogin()
        {
            if (mAuthTask != null)
            {
                return;
            }

            // Reset errors.
            mEmailView.SetError((string)null, null);
            mPasswordView.SetError((string)null, null);

            //// Store values at the time of the login attempt.
            string email = mEmailView.Text;
            string password = mPasswordView.Text;

            bool cancel = false;
            View focusView = null;

            // Check for a valid password, if the user entered one.
            if (!TextUtils.IsEmpty(password) && !IsPasswordValid(password))
            {
                mPasswordView.SetError(GetString(Resource.String.error_invalid_password), null);
                focusView = mPasswordView;
                cancel = true;
            }

            // Check for a valid email address.
            if (TextUtils.IsEmpty(email))
            {
                mEmailView.SetError(GetString(Resource.String.error_field_required), null);
                focusView = mEmailView;
                cancel = true;
            }
            else if (!IsEmailValid(email))
            {
                mEmailView.SetError(GetString(Resource.String.error_invalid_email), null);
                focusView = mEmailView;
                cancel = true;
            }

            if (cancel)
            {
                // There was an error; don't attempt login and focus the first
                // form field with an error.
                focusView.RequestFocus();
            }
            else
            {
                // Show a progress spinner, and kick off a background task to
                // perform the user login attempt.
                ShowProgress(true);
                mAuthTask = new UserLoginTask(email, password, this);
                mAuthTask.Execute((Void)null);
            }
        }
        
        private bool IsEmailValid(string email)
        {
            //TODO: Replace this with your own logic
            return email.Contains("@");
        }

        private bool IsPasswordValid(string password)
        {
            //TODO: Replace this with your own logic
            return password.Length > 4;
        }

        [TargetApi(Value = (int)BuildVersionCodes.HoneycombMr2)]
        private void ShowProgress(bool show)
        {
            // On Honeycomb MR2 we have the ViewPropertyAnimator APIs, which allow
            // for very easy animations. If available, use these APIs to fade-in
            // the progress spinner.
            if (Build.VERSION.SdkInt >= BuildVersionCodes.HoneycombMr2)
            {
                int shortAnimTime = Resources.GetInteger(Android.Resource.Integer.ConfigShortAnimTime);

                mLoginFormView.Visibility = show ? ViewStates.Gone : ViewStates.Visible;
                mLoginFormView.Animate().SetDuration(shortAnimTime).Alpha(
                        show ? 0 : 1).WithEndAction(new Runnable(() =>
                        {
                            mLoginFormView.Visibility = show ? ViewStates.Gone : ViewStates.Visible;
                        }));

                mProgressView.Visibility = show ? ViewStates.Visible : ViewStates.Gone;
                mProgressView.Animate().SetDuration(shortAnimTime).Alpha(
                    show ? 1 : 0).WithEndAction(new Runnable(() =>
                    {
                        mProgressView.Visibility = show ? ViewStates.Visible : ViewStates.Gone;
                    }));
            }
            else
            {
                // The ViewPropertyAnimator APIs are not available, so simply show
                // and hide the relevant UI components.
                mLoginFormView.Visibility = show ? ViewStates.Visible : ViewStates.Gone;
                mProgressView.Visibility = show ? ViewStates.Gone : ViewStates.Visible;
            }
        }

        public Loader OnCreateLoader(int id, Bundle args)
        {
            return new CursorLoader(this,
                    // Retrieve data rows for the device user's 'profile' contact.
                    Uri.WithAppendedPath(ContactsContract.Profile.ContentUri,
                            ContactsContract.Contacts.Data.ContentDirectory), ProfileQuery.PROJECTION,

                    // Select only email addresses.
                    ContactsContract.Data.InterfaceConsts.Mimetype +
                            " = ?", new string[]{ContactsContract.CommonDataKinds.Email.ContentItemType},

                    // Show primary email addresses first. Note that there won't be
                    // a primary email address if the user hasn't specified one.
                    ContactsContract.Data.InterfaceConsts.IsPrimary + " DESC");
        }

        public void OnLoaderReset(Loader loader)
        {
        }

        public void OnLoadFinished(Loader loader, Object data)
        {
            List<string> emails = new List<string>();
            var cursorLoader = loader as CursorLoader;
            var cursor = cursorLoader?.LoadInBackground().JavaCast<ICursor>();
            cursor.MoveToFirst();
            while (!cursor.IsAfterLast)
            {
                emails.Add(cursor.GetString(ProfileQuery.ADDRESS));
                cursor.MoveToNext();
            }

            AddEmailsToAutoComplete(emails);
        }

        private void AddEmailsToAutoComplete(List<string> emailAddressCollection)
        {
            //Create adapter to tell the AutoCompleteTextView what to show in its dropdown list.
            ArrayAdapter<string> adapter =
                    new ArrayAdapter<string>(this,
                            Android.Resource.Layout.SimpleDropDownItem1Line, emailAddressCollection);

            mEmailView.Adapter = adapter;
        }

        private static class ProfileQuery
        {
            public static string[] PROJECTION = {
                ContactsContract.CommonDataKinds.Email.Address,
                //Android.Provider.ContactsContract.CommonDataKinds.Email.IS_PRIMARY,
        };

            public static int ADDRESS = 0;
            public static int IS_PRIMARY = 1;
        }
        public class UserLoginTask : AsyncTask
        {
            MainActivity instance;

            private string mEmail;
            private string mPassword;

            public UserLoginTask(string email, string password, MainActivity instance)
            {
                this.instance = instance;
                mEmail = email;
                mPassword = password;
            }

            protected override Object DoInBackground(params Object[] @params)
            {
                // TODO: attempt authentication against a network service.

                try
                {
                    // Simulate network access.
                    Thread.Sleep(2000);
                }
                catch (InterruptedException e)
                {
                    return false;
                }

                foreach (string credential in DUMMY_CREDENTIALS)
                {
                    string[] pieces = credential.Split(':');
                    if (pieces[0].Equals(mEmail))
                    {
                        // Account exists, return true if the password matches.
                        return pieces[1].Equals(mPassword);
                    }
                }

                // TODO: register the new account here.
                return true;
            }

            protected override void OnPostExecute(Object result)
            {
                instance.mAuthTask = null;
                instance.ShowProgress(false);

                if ((bool)result)
                {
                    instance.Finish();
                }
                else
                {
                    instance.mPasswordView.Error = instance.GetString(Resource.String.error_incorrect_password);
                    instance.mPasswordView.RequestFocus();
                }
            }
            
            protected override void OnCancelled()
            {
                instance.mAuthTask = null;
                instance.ShowProgress(false);
            }
        }
    }
}

