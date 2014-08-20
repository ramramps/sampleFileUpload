/* This is just a sample program that uses MultipartFormDataContent for file upload - each byte transferred can be tracked */
//doc - single object that will be sent along with the document 
//docKeyword - List object that will be sent along with the document

//ignore doc and dockeyword if you are going to save only the document and not any parameters with the document

public static async Task<HttpResponseMessage> FileUpload(String fileLocation, Document doc,
                                                            List<DocumentKeyword> docKeyword, Boolean AddToIndexedQueue)
       {
           try
           {
              
               String url = String.Empty;
               String message = String.Empty;              
               String mapping = MimeMapping.GetMimeMapping(fileLocation);

               Dictionary<string, object> Parameters = new Dictionary<string, object>();

               Parameters.Add("Document", JsonConvert.SerializeObject(doc));

               if (!AddToIndexedQueue)
               {
                   //extension refers to a static class where I get the URL parameter from web config file                                                        
                   url = Extensions.GetConfigurationKey("WebDocumentURL") + "/api/Document/UnIndexedDocument/Add";
               }

               else
               {
                   url = Extensions.GetConfigurationKey("WebDocumentURL") + "/api/Document/Index";
                   IList<DocumentKeyword> kyword = docKeyword.Cast<DocumentKeyword>().ToList();
                   Parameters.Add("Keywords", JsonConvert.SerializeObject(kyword));
               }

               MultipartFormDataContent formdata = new MultipartFormDataContent();
               
               foreach (var param in Parameters)
               {
                   formdata.Add(new StringContent(param.Value.ToString()), param.Key);
               }

              
               HttpClientHandler handler = new HttpClientHandler
               {
                   UseDefaultCredentials = true,
               };

               ProgressMessageHandler messageHandler = new ProgressMessageHandler();
               messageHandler.InnerHandler = handler;
               messageHandler.HttpSendProgress += messageHandler_HttpSendProgress;

              //Update form is where the progress bar resides
               if (updateForm != null) updateForm.Close();
               updateForm = new UpdateProgressForm();
               updateForm.Text = "File Upload";
               updateForm.Show();

              
               using (FileStream fileStream = new FileStream(fileLocation, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
               {
                   StreamContent streamContent = new StreamContent(fileStream);

                   streamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                   {
                       Name = "\"file\"",
                       FileName = "\"" + Path.GetFileName(fileLocation) + "\""
                   };
                   
                   streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                   HttpResponseMessage responseMessage = null;
                   using (var client = new HttpClient(messageHandler))
                   {
                       client.Timeout = TimeSpan.FromSeconds(30000);

                       // formdata.Add(byteContent, "file");
                       formdata.Add(streamContent, "file");
                       
                       //send the document and document parameters to the server
                       await client.PostAsync(url, formdata).ContinueWith(
                         t =>
                         {
                             if (!t.IsFaulted || t.IsCanceled)
                             {
                                 responseMessage = t.Result; 
                                 
                                 // do something with responseMessage
                             }
                         });

                       return responseMessage;
                   }
               }
               
           }
           catch (Exception ex)
           {
               WebApiRequest.WriteToErrorLog(ex.Message.ToString(), ex.StackTrace.ToString(), "Saving Document");
               PostError(ex, false);
               System.Windows.Forms.MessageBox.Show(ex.Message.ToString(), "Error", System.Windows.Forms.MessageBoxButtons.OK);
               return null;
           }

       }
       
      
       
       /// <summary>
       /// Handles the HttpSendProgress event of the messageHandler control.
       /// This is for file upload
       /// </summary>
       /// <param name="sender">The source of the event.</param>
       /// <param name="e">The <see cref="HttpProgressEventArgs" /> instance containing the event data.</param>
       //updateform - form where the progress bar resides
       
       private static void messageHandler_HttpSendProgress(object sender, HttpProgressEventArgs e)
       {
           try
           {
               updateForm.updateProgressMessage(e.ProgressPercentage);
               var difference = e.BytesTransferred - totalbytestransferred;

               if (difference != 0)
               {
                   totalbytestransferred = e.BytesTransferred;
                   var totalBytesLoaded = e.BytesTransferred;
                   difference = difference * 2;
                   double bytesRemaining = (double)e.TotalBytes - e.BytesTransferred;
                   double secondsRemaining = Convert.ToDouble(bytesRemaining / difference);
                   //updateForm.updateMessage(convertToTime(secondsRemaining));
                   updateForm.updateMessage(CalculateETA(Convert.ToDouble(e.BytesTransferred), Convert.ToDouble(e.TotalBytes)));
               }
               if (e.ProgressPercentage == 100)
                   updateForm.Close();
           }
           catch (Exception ex)
           {
               WebApiRequest.PostError(ex, false);
           }
       }

       public static String CalculateETA(double bytes, double totalBytes )
       {
           Int32 pc = Convert.ToInt32(100 - (bytes / totalBytes * 100));
           Int32 pci = Convert.ToInt32(bytes / totalBytes * 100);

           double pcia = Convert.ToDouble(bytes / 1024);
           double pcia2 = Convert.ToDouble(totalBytes / 1024);
           double eta;                    
           if (pcia > 1024)
           {
               pcia = pcia / 1024;
               pcia2 = pcia2 / 1024;
               TimeSpan elapsed = DateTime.Now.TimeOfDay - startTime.TimeOfDay;               
               double elapsedTime = elapsed.TotalSeconds;
               double sentRate = Convert.ToDouble(bytes / elapsedTime);
               eta = Convert.ToDouble((totalBytes - bytes) / sentRate);
               eta = Math.Round(eta);
           }
           else
           {
               TimeSpan elapsed = DateTime.Now.TimeOfDay - startTime.TimeOfDay;
               double elapsedTime = elapsed.TotalSeconds;
               double sentRate = Convert.ToDouble(bytes / elapsedTime);
               eta = Convert.ToDouble((totalBytes - bytes) / sentRate);
               eta = Math.Round(eta); ;
           }

           return convertToTime(eta);
           
       }

       /// <summary>
       /// Calculates the time remaining
       /// </summary>
       /// <param name="secs">The secs.</param>
       /// <returns></returns>
       private static String convertToTime(double secs)
       {
           String hour = "00";
           String minutes = "00";
           String seconds = "00";

           var hr = Math.Floor(secs / 3600);
           var min = Math.Floor((secs - (hr * 3600)) / 60);
           var sec = Math.Floor(secs - (hr * 3600) - (min * 60));

           if (hr < 10)
           {
               hour = hr.ToString(); hour = "0" + hour;
           }
           else
           {
               hour = hr.ToString();
           }
           if (min < 10)
           {
               minutes = min.ToString(); minutes = "0" + minutes;
           }
           else
           {
               minutes = min.ToString();
           }
           if (sec < 10)
           {
               seconds = sec.ToString(); seconds = "0" + seconds;
           }
           else
           {
               seconds = sec.ToString();
           }
           return "Time Remaining : " + hour + ":" + minutes + ":" + seconds;                    
       }
       
