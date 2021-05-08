using System;
using System.Collections.Generic;
using System.Text;

namespace CowinPoll.Models
{
    public class Response<T>
    {
        public T Content { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }
}
