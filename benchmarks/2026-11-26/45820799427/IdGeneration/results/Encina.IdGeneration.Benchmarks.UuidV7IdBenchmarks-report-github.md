```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.55GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 492.9 ns |  11.83 ns |  7.82 ns |  1.03 |    0.02 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 438.6 ns |  16.60 ns | 10.98 ns |  0.92 |    0.03 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 478.8 ns |  11.25 ns |  7.44 ns |  1.00 |    0.02 |         - |          NA |
|                    |            |                |             |          |           |          |       |         |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 500.4 ns | 322.13 ns | 17.66 ns |  1.04 |    0.04 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 425.1 ns |  46.72 ns |  2.56 ns |  0.88 |    0.02 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 481.4 ns | 188.69 ns | 10.34 ns |  1.00 |    0.03 |         - |          NA |
