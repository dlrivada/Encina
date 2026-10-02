```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method              | Job        | IterationCount | LaunchCount | Mean      | Error      | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |----------:|-----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     | 101.02 ns |   0.647 ns | 0.428 ns |  1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 410.68 ns |   2.744 ns | 1.633 ns |  4.07 |    0.02 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 491.55 ns |   3.439 ns | 2.047 ns |  4.87 |    0.03 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 401.65 ns |   1.586 ns | 1.049 ns |  3.98 |    0.02 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 184.60 ns |   0.624 ns | 0.371 ns |  1.83 |    0.01 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     | 123.47 ns |   0.641 ns | 0.424 ns |  1.22 |    0.01 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 228.64 ns |   1.396 ns | 0.831 ns |  2.26 |    0.01 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 183.14 ns |   0.330 ns | 0.196 ns |  1.81 |    0.01 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  63.69 ns |   0.220 ns | 0.131 ns |  0.63 |    0.00 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  74.73 ns |   0.308 ns | 0.161 ns |  0.74 |    0.00 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     | 108.15 ns |   0.881 ns | 0.583 ns |  1.07 |    0.01 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 572.76 ns |   2.775 ns | 1.836 ns |  5.67 |    0.03 | 0.0248 |     416 B |        1.86 |
|                     |            |                |             |           |            |          |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           |  99.98 ns |   0.910 ns | 0.050 ns |  1.00 |    0.00 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 409.06 ns |  29.197 ns | 1.600 ns |  4.09 |    0.01 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 493.66 ns | 114.766 ns | 6.291 ns |  4.94 |    0.05 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 398.05 ns |  25.895 ns | 1.419 ns |  3.98 |    0.01 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           | 185.80 ns |  13.542 ns | 0.742 ns |  1.86 |    0.01 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           | 125.46 ns |   8.850 ns | 0.485 ns |  1.25 |    0.00 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 225.01 ns |  12.245 ns | 0.671 ns |  2.25 |    0.01 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           | 182.97 ns |   5.761 ns | 0.316 ns |  1.83 |    0.00 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  63.65 ns |   4.716 ns | 0.258 ns |  0.64 |    0.00 | 0.0076 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  76.27 ns |   5.936 ns | 0.325 ns |  0.76 |    0.00 | 0.0076 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           | 109.41 ns |  35.040 ns | 1.921 ns |  1.09 |    0.02 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 586.00 ns |  19.383 ns | 1.062 ns |  5.86 |    0.01 | 0.0248 |     416 B |        1.86 |
