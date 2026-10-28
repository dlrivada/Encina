```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.06GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method              | Job        | IterationCount | LaunchCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     |  56.97 ns |   1.111 ns |  0.735 ns |  1.00 |    0.02 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 239.88 ns |   7.430 ns |  4.915 ns |  4.21 |    0.10 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 301.10 ns |  15.653 ns | 10.354 ns |  5.29 |    0.18 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 238.20 ns |   9.984 ns |  6.604 ns |  4.18 |    0.12 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 104.79 ns |   5.005 ns |  2.978 ns |  1.84 |    0.05 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     |  64.23 ns |   1.921 ns |  1.005 ns |  1.13 |    0.02 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 137.73 ns |   5.461 ns |  3.612 ns |  2.42 |    0.07 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 106.82 ns |   4.082 ns |  2.700 ns |  1.88 |    0.05 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  40.11 ns |   0.631 ns |  0.375 ns |  0.70 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  40.10 ns |   1.531 ns |  0.911 ns |  0.70 |    0.02 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     |  59.24 ns |   3.480 ns |  2.302 ns |  1.04 |    0.04 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 370.77 ns |  17.228 ns | 11.395 ns |  6.51 |    0.21 | 0.0234 |     392 B |        1.75 |
|                     |            |                |             |           |            |           |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           |  56.09 ns |  32.085 ns |  1.759 ns |  1.00 |    0.04 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 257.19 ns |  23.021 ns |  1.262 ns |  4.59 |    0.13 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 295.14 ns | 188.970 ns | 10.358 ns |  5.27 |    0.21 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 235.55 ns |  55.147 ns |  3.023 ns |  4.20 |    0.12 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           | 103.44 ns |  10.572 ns |  0.579 ns |  1.85 |    0.05 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           |  60.40 ns |  10.719 ns |  0.588 ns |  1.08 |    0.03 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 138.52 ns | 260.603 ns | 14.285 ns |  2.47 |    0.23 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           | 107.07 ns |  52.298 ns |  2.867 ns |  1.91 |    0.07 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  40.41 ns |   9.404 ns |  0.515 ns |  0.72 |    0.02 | 0.0076 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  42.10 ns |  34.262 ns |  1.878 ns |  0.75 |    0.04 | 0.0076 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           |  60.46 ns |  47.369 ns |  2.596 ns |  1.08 |    0.05 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 382.62 ns |  26.419 ns |  1.448 ns |  6.83 |    0.19 | 0.0234 |     392 B |        1.75 |
