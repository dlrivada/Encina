```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 629.9 ns |   3.26 ns | 2.16 ns |  1.01 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 399.2 ns |   0.42 ns | 0.22 ns |  0.64 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 624.1 ns |   8.12 ns | 5.37 ns |  1.00 |         - |          NA |
|                    |            |                |             |          |           |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 612.4 ns | 119.52 ns | 6.55 ns |  0.98 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 397.3 ns |   4.30 ns | 0.24 ns |  0.64 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 621.8 ns |  68.42 ns | 3.75 ns |  1.00 |         - |          NA |
