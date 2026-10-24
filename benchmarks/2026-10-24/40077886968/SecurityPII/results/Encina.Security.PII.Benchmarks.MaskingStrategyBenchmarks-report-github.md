```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method              | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     |  79.47 ns |  0.211 ns | 0.126 ns |  1.00 |    0.00 | 0.0026 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 339.71 ns |  0.871 ns | 0.576 ns |  4.27 |    0.01 | 0.0062 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 402.25 ns |  0.752 ns | 0.498 ns |  5.06 |    0.01 | 0.0062 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 327.96 ns |  1.849 ns | 1.223 ns |  4.13 |    0.02 | 0.0062 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 149.61 ns |  0.409 ns | 0.243 ns |  1.88 |    0.00 | 0.0033 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     |  88.32 ns |  1.361 ns | 0.901 ns |  1.11 |    0.01 | 0.0038 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 197.33 ns |  0.859 ns | 0.511 ns |  2.48 |    0.01 | 0.0045 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 143.06 ns |  0.158 ns | 0.094 ns |  1.80 |    0.00 | 0.0031 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  44.08 ns |  0.154 ns | 0.102 ns |  0.55 |    0.00 | 0.0015 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  55.04 ns |  0.054 ns | 0.032 ns |  0.69 |    0.00 | 0.0015 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     |  84.59 ns |  0.076 ns | 0.040 ns |  1.06 |    0.00 | 0.0038 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 466.59 ns |  0.616 ns | 0.367 ns |  5.87 |    0.01 | 0.0043 |     392 B |        1.75 |
|                     |            |                |             |           |           |          |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           |  80.14 ns |  3.285 ns | 0.180 ns |  1.00 |    0.00 | 0.0026 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 337.87 ns |  9.895 ns | 0.542 ns |  4.22 |    0.01 | 0.0062 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 410.32 ns |  4.573 ns | 0.251 ns |  5.12 |    0.01 | 0.0062 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 331.16 ns | 16.124 ns | 0.884 ns |  4.13 |    0.01 | 0.0062 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           | 154.23 ns |  3.768 ns | 0.207 ns |  1.92 |    0.00 | 0.0033 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           |  94.80 ns |  7.012 ns | 0.384 ns |  1.18 |    0.00 | 0.0038 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 200.98 ns |  3.551 ns | 0.195 ns |  2.51 |    0.01 | 0.0045 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           | 147.16 ns |  4.560 ns | 0.250 ns |  1.84 |    0.00 | 0.0031 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  44.62 ns |  1.370 ns | 0.075 ns |  0.56 |    0.00 | 0.0015 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  55.69 ns |  1.373 ns | 0.075 ns |  0.69 |    0.00 | 0.0015 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           |  84.89 ns |  2.863 ns | 0.157 ns |  1.06 |    0.00 | 0.0038 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 470.20 ns | 14.995 ns | 0.822 ns |  5.87 |    0.01 | 0.0043 |     392 B |        1.75 |
