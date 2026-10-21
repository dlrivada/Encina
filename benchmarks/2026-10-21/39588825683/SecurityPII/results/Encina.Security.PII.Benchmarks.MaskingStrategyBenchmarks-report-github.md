```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.53GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method              | Job        | IterationCount | LaunchCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     |  52.59 ns |   0.778 ns |  0.463 ns |  1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 220.49 ns |   3.865 ns |  2.300 ns |  4.19 |    0.05 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 270.56 ns |   6.637 ns |  3.949 ns |  5.14 |    0.08 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 209.36 ns |   3.755 ns |  1.964 ns |  3.98 |    0.05 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     |  96.79 ns |   2.080 ns |  1.238 ns |  1.84 |    0.03 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     |  57.32 ns |   0.468 ns |  0.279 ns |  1.09 |    0.01 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 121.68 ns |   3.131 ns |  2.071 ns |  2.31 |    0.04 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     |  91.76 ns |   1.079 ns |  0.714 ns |  1.74 |    0.02 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  35.13 ns |   0.295 ns |  0.195 ns |  0.67 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  37.00 ns |   0.492 ns |  0.293 ns |  0.70 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     |  53.83 ns |   0.885 ns |  0.586 ns |  1.02 |    0.01 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 342.89 ns |   8.379 ns |  4.986 ns |  6.52 |    0.11 | 0.0234 |     392 B |        1.75 |
|                     |            |                |             |           |            |           |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           |  50.82 ns |   5.934 ns |  0.325 ns |  1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 225.03 ns | 204.031 ns | 11.184 ns |  4.43 |    0.19 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 267.05 ns |  83.186 ns |  4.560 ns |  5.26 |    0.08 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 223.10 ns |  80.490 ns |  4.412 ns |  4.39 |    0.08 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           |  98.09 ns |  31.147 ns |  1.707 ns |  1.93 |    0.03 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           |  55.23 ns |   8.671 ns |  0.475 ns |  1.09 |    0.01 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 122.58 ns |  14.015 ns |  0.768 ns |  2.41 |    0.02 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           |  95.14 ns |  39.775 ns |  2.180 ns |  1.87 |    0.04 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  38.21 ns |  22.483 ns |  1.232 ns |  0.75 |    0.02 | 0.0076 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  37.85 ns |  10.098 ns |  0.554 ns |  0.74 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           |  55.76 ns |   5.668 ns |  0.311 ns |  1.10 |    0.01 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 342.80 ns |  60.828 ns |  3.334 ns |  6.75 |    0.07 | 0.0234 |     392 B |        1.75 |
