```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method              | Job        | IterationCount | LaunchCount | Mean      | Error      | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |----------:|-----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     | 105.83 ns |   1.132 ns | 0.749 ns |  1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 415.99 ns |   2.139 ns | 1.273 ns |  3.93 |    0.03 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 485.53 ns |   7.326 ns | 4.846 ns |  4.59 |    0.05 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 429.14 ns |   3.212 ns | 2.124 ns |  4.06 |    0.03 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 183.51 ns |   1.269 ns | 0.839 ns |  1.73 |    0.01 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     | 127.27 ns |   1.224 ns | 0.810 ns |  1.20 |    0.01 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 232.68 ns |   1.973 ns | 1.305 ns |  2.20 |    0.02 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 185.52 ns |   1.504 ns | 0.995 ns |  1.75 |    0.01 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  60.46 ns |   0.477 ns | 0.284 ns |  0.57 |    0.00 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  76.63 ns |   0.304 ns | 0.159 ns |  0.72 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     | 108.76 ns |   1.524 ns | 1.008 ns |  1.03 |    0.01 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 634.77 ns |   3.469 ns | 2.294 ns |  6.00 |    0.05 | 0.0229 |     392 B |        1.75 |
|                     |            |                |             |           |            |          |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           | 102.38 ns |  12.858 ns | 0.705 ns |  1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 414.02 ns |  26.633 ns | 1.460 ns |  4.04 |    0.03 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 504.38 ns |  72.391 ns | 3.968 ns |  4.93 |    0.04 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 411.37 ns |  52.145 ns | 2.858 ns |  4.02 |    0.03 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           | 188.05 ns |  16.735 ns | 0.917 ns |  1.84 |    0.01 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           | 129.18 ns |  37.835 ns | 2.074 ns |  1.26 |    0.02 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 239.06 ns |  32.046 ns | 1.757 ns |  2.34 |    0.02 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           | 205.47 ns |  28.360 ns | 1.554 ns |  2.01 |    0.02 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  61.00 ns |   6.371 ns | 0.349 ns |  0.60 |    0.00 | 0.0076 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  77.64 ns |   2.958 ns | 0.162 ns |  0.76 |    0.00 | 0.0076 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           | 108.30 ns |   8.368 ns | 0.459 ns |  1.06 |    0.01 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 672.83 ns | 154.395 ns | 8.463 ns |  6.57 |    0.08 | 0.0229 |     392 B |        1.75 |
