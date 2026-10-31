```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.73GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method              | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     | 109.08 ns |  0.986 ns | 0.652 ns |  1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 420.29 ns |  3.698 ns | 2.446 ns |  3.85 |    0.03 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 498.37 ns |  2.509 ns | 1.660 ns |  4.57 |    0.03 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 419.41 ns |  2.313 ns | 1.530 ns |  3.85 |    0.03 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 190.43 ns |  1.635 ns | 1.082 ns |  1.75 |    0.01 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     | 129.47 ns |  1.125 ns | 0.744 ns |  1.19 |    0.01 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 247.59 ns |  3.564 ns | 2.357 ns |  2.27 |    0.02 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 198.16 ns |  1.407 ns | 0.931 ns |  1.82 |    0.01 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  64.63 ns |  0.306 ns | 0.182 ns |  0.59 |    0.00 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  79.41 ns |  0.607 ns | 0.401 ns |  0.73 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     | 116.66 ns |  1.445 ns | 0.956 ns |  1.07 |    0.01 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 645.85 ns |  3.722 ns | 2.462 ns |  5.92 |    0.04 | 0.0229 |     392 B |        1.75 |
|                     |            |                |             |           |           |          |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           | 107.52 ns |  8.053 ns | 0.441 ns |  1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 421.51 ns | 20.110 ns | 1.102 ns |  3.92 |    0.02 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 511.91 ns | 41.564 ns | 2.278 ns |  4.76 |    0.02 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 424.41 ns |  8.840 ns | 0.485 ns |  3.95 |    0.01 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           | 195.77 ns |  3.353 ns | 0.184 ns |  1.82 |    0.01 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           | 134.86 ns | 11.699 ns | 0.641 ns |  1.25 |    0.01 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 240.65 ns | 12.907 ns | 0.707 ns |  2.24 |    0.01 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           | 196.62 ns | 26.210 ns | 1.437 ns |  1.83 |    0.01 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  66.84 ns | 10.887 ns | 0.597 ns |  0.62 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  78.02 ns |  7.286 ns | 0.399 ns |  0.73 |    0.00 | 0.0076 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           | 116.34 ns | 32.920 ns | 1.804 ns |  1.08 |    0.02 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 648.18 ns | 21.784 ns | 1.194 ns |  6.03 |    0.02 | 0.0229 |     392 B |        1.75 |
