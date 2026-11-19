```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.99GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 768.9 ns |   5.94 ns | 3.93 ns |  0.95 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 668.0 ns |   3.41 ns | 2.03 ns |  0.83 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 809.5 ns |   2.29 ns | 1.20 ns |  1.00 |         - |          NA |
|                    |            |                |             |          |           |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 809.9 ns | 109.01 ns | 5.98 ns |  0.99 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 663.5 ns |   6.66 ns | 0.36 ns |  0.81 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 814.7 ns |   5.60 ns | 0.31 ns |  1.00 |         - |          NA |
