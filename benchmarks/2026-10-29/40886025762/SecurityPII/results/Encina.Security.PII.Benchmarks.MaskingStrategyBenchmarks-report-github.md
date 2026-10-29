```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method              | Job        | IterationCount | LaunchCount | Mean      | Error      | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |----------:|-----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     |  75.33 ns |   1.933 ns | 1.279 ns |  1.00 |    0.02 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 342.11 ns |   2.688 ns | 1.406 ns |  4.54 |    0.08 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 387.47 ns |   1.997 ns | 1.321 ns |  5.15 |    0.09 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 311.38 ns |   3.009 ns | 1.574 ns |  4.13 |    0.07 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 137.87 ns |   1.644 ns | 1.088 ns |  1.83 |    0.03 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     |  86.77 ns |   0.276 ns | 0.144 ns |  1.15 |    0.02 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 177.96 ns |   1.589 ns | 1.051 ns |  2.36 |    0.04 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 137.31 ns |   1.583 ns | 1.047 ns |  1.82 |    0.03 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  49.77 ns |   0.080 ns | 0.047 ns |  0.66 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  56.50 ns |   0.099 ns | 0.052 ns |  0.75 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     |  82.39 ns |   0.599 ns | 0.396 ns |  1.09 |    0.02 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 513.72 ns |   0.830 ns | 0.549 ns |  6.82 |    0.11 | 0.0229 |     392 B |        1.75 |
|                     |            |                |             |           |            |          |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           |  77.57 ns |  42.371 ns | 2.322 ns |  1.00 |    0.04 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 333.28 ns | 127.580 ns | 6.993 ns |  4.30 |    0.14 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 396.90 ns |  32.564 ns | 1.785 ns |  5.12 |    0.14 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 322.74 ns |  56.731 ns | 3.110 ns |  4.16 |    0.12 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           | 137.90 ns |   1.268 ns | 0.069 ns |  1.78 |    0.05 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           |  84.83 ns |   5.975 ns | 0.328 ns |  1.09 |    0.03 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 177.70 ns |  18.526 ns | 1.015 ns |  2.29 |    0.06 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           | 140.80 ns |   5.714 ns | 0.313 ns |  1.82 |    0.05 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  50.59 ns |   2.991 ns | 0.164 ns |  0.65 |    0.02 | 0.0076 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  57.03 ns |   1.658 ns | 0.091 ns |  0.74 |    0.02 | 0.0076 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           |  80.95 ns |   9.961 ns | 0.546 ns |  1.04 |    0.03 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 496.38 ns |  16.888 ns | 0.926 ns |  6.40 |    0.17 | 0.0229 |     392 B |        1.75 |
