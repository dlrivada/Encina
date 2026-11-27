```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.86GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 842.1 ns |   6.18 ns |  3.68 ns |  1.00 |    0.00 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 877.9 ns |   3.95 ns |  2.61 ns |  1.04 |    0.00 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 840.7 ns |   4.35 ns |  2.28 ns |  1.00 |    0.00 |         - |          NA |
|                    |            |                |             |          |           |          |       |         |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 859.1 ns | 312.81 ns | 17.15 ns |  1.01 |    0.02 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 880.3 ns |  64.31 ns |  3.52 ns |  1.04 |    0.00 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 846.8 ns |  50.32 ns |  2.76 ns |  1.00 |    0.00 |         - |          NA |
