```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.29GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 514.8 ns |  19.83 ns | 13.12 ns |  1.01 |    0.04 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 440.4 ns |   5.94 ns |  3.54 ns |  0.87 |    0.03 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 509.2 ns |  24.65 ns | 16.31 ns |  1.00 |    0.04 |         - |          NA |
|                    |            |                |             |          |           |          |       |         |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 515.2 ns | 279.17 ns | 15.30 ns |  1.02 |    0.04 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 435.7 ns |  47.68 ns |  2.61 ns |  0.86 |    0.02 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 504.4 ns | 268.51 ns | 14.72 ns |  1.00 |    0.04 |         - |          NA |
