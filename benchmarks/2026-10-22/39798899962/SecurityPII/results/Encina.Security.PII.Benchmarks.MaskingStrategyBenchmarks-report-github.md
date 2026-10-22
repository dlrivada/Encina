```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 4.04GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method              | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     |  68.01 ns |  1.116 ns |  0.584 ns |  1.00 |    0.01 | 0.0026 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 291.04 ns |  0.965 ns |  0.574 ns |  4.28 |    0.03 | 0.0062 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 347.29 ns |  0.771 ns |  0.510 ns |  5.11 |    0.04 | 0.0062 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 294.72 ns | 32.146 ns | 21.262 ns |  4.33 |    0.30 | 0.0062 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 132.09 ns |  0.955 ns |  0.632 ns |  1.94 |    0.02 | 0.0033 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     |  79.62 ns |  0.263 ns |  0.174 ns |  1.17 |    0.01 | 0.0038 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 171.62 ns |  2.329 ns |  1.541 ns |  2.52 |    0.03 | 0.0045 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 138.12 ns | 13.588 ns |  8.988 ns |  2.03 |    0.13 | 0.0031 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  39.80 ns |  0.143 ns |  0.095 ns |  0.59 |    0.00 | 0.0015 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  48.59 ns |  0.447 ns |  0.266 ns |  0.71 |    0.01 | 0.0015 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     |  74.90 ns |  0.370 ns |  0.245 ns |  1.10 |    0.01 | 0.0038 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 409.12 ns | 18.441 ns | 10.974 ns |  6.02 |    0.16 | 0.0043 |     392 B |        1.75 |
|                     |            |                |             |           |           |           |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           |  68.68 ns |  9.416 ns |  0.516 ns |  1.00 |    0.01 | 0.0026 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 288.40 ns | 50.214 ns |  2.752 ns |  4.20 |    0.04 | 0.0062 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 347.59 ns |  2.869 ns |  0.157 ns |  5.06 |    0.03 | 0.0062 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 318.11 ns | 31.356 ns |  1.719 ns |  4.63 |    0.04 | 0.0062 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           | 129.13 ns | 27.485 ns |  1.507 ns |  1.88 |    0.02 | 0.0033 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           |  78.59 ns | 60.303 ns |  3.305 ns |  1.14 |    0.04 | 0.0038 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 169.44 ns | 13.223 ns |  0.725 ns |  2.47 |    0.02 | 0.0045 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           | 138.54 ns | 43.559 ns |  2.388 ns |  2.02 |    0.03 | 0.0031 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  40.70 ns |  8.245 ns |  0.452 ns |  0.59 |    0.01 | 0.0015 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  48.46 ns |  9.530 ns |  0.522 ns |  0.71 |    0.01 | 0.0015 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           |  74.31 ns |  4.778 ns |  0.262 ns |  1.08 |    0.01 | 0.0038 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 400.37 ns | 17.908 ns |  0.982 ns |  5.83 |    0.04 | 0.0043 |     392 B |        1.75 |
