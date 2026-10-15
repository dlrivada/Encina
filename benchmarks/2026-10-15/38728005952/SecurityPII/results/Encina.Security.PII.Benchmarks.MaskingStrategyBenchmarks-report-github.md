```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method              | Job        | IterationCount | LaunchCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     |  56.20 ns |   2.017 ns |  1.334 ns |  1.00 |    0.03 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 234.12 ns |   9.821 ns |  6.496 ns |  4.17 |    0.14 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 290.97 ns |  11.664 ns |  6.941 ns |  5.18 |    0.16 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 223.00 ns |   2.736 ns |  1.810 ns |  3.97 |    0.09 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 102.56 ns |   1.291 ns |  0.854 ns |  1.83 |    0.04 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     |  63.63 ns |   1.679 ns |  1.111 ns |  1.13 |    0.03 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 126.71 ns |   2.697 ns |  1.605 ns |  2.26 |    0.06 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     |  98.15 ns |   2.923 ns |  1.933 ns |  1.75 |    0.05 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  38.91 ns |   0.921 ns |  0.609 ns |  0.69 |    0.02 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  41.54 ns |   2.168 ns |  1.434 ns |  0.74 |    0.03 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     |  55.46 ns |   1.323 ns |  0.875 ns |  0.99 |    0.03 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 347.42 ns |  14.029 ns |  9.280 ns |  6.18 |    0.21 | 0.0234 |     392 B |        1.75 |
|                     |            |                |             |           |            |           |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           |  53.62 ns |  24.815 ns |  1.360 ns |  1.00 |    0.03 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 226.49 ns |  31.623 ns |  1.733 ns |  4.23 |    0.10 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 276.03 ns |  81.339 ns |  4.458 ns |  5.15 |    0.13 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 220.82 ns | 182.885 ns | 10.025 ns |  4.12 |    0.19 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           | 100.12 ns |  86.366 ns |  4.734 ns |  1.87 |    0.09 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           |  57.85 ns |  20.644 ns |  1.132 ns |  1.08 |    0.03 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 126.18 ns |  41.683 ns |  2.285 ns |  2.35 |    0.06 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           |  95.90 ns |  23.059 ns |  1.264 ns |  1.79 |    0.04 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  37.32 ns |  23.863 ns |  1.308 ns |  0.70 |    0.03 | 0.0076 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  38.60 ns |   2.219 ns |  0.122 ns |  0.72 |    0.02 | 0.0076 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           |  58.15 ns |  51.184 ns |  2.806 ns |  1.08 |    0.05 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 355.54 ns |  77.823 ns |  4.266 ns |  6.63 |    0.16 | 0.0234 |     392 B |        1.75 |
