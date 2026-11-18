```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.52GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev  | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|--------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 494.3 ns |   7.66 ns | 4.56 ns |  1.01 |    0.02 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 424.0 ns |   2.14 ns | 1.28 ns |  0.87 |    0.02 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 489.1 ns |  14.52 ns | 9.61 ns |  1.00 |    0.03 |         - |          NA |
|                    |            |                |             |          |           |         |       |         |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 492.5 ns | 170.40 ns | 9.34 ns |  1.01 |    0.02 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 426.7 ns |  72.18 ns | 3.96 ns |  0.87 |    0.01 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 489.5 ns |  22.31 ns | 1.22 ns |  1.00 |    0.00 |         - |          NA |
