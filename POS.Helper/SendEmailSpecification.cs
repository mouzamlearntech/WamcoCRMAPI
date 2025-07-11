﻿using System.Collections.Generic;

namespace POS.Helper
{
    public class SendEmailSpecification
    {
        public string FromAddress { get; set; }
        public string FromName { get; set; } = "";
        public string ToName { get; set; } = "";
        public string ToAddress { get; set; }
        public string CCAddress { get; set; }
        public string Body { get; set; }
        public string Subject { get; set; }
        public string EncryptionType { get; set; }
        public int Port { get; set; }
        public string Host { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public List<FileInfo> Attechments { get; set; } = new();
    }
}
