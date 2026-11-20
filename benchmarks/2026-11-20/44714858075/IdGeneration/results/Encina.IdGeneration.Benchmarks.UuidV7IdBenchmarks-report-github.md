```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 801.4 ns |   2.61 ns | 1.36 ns |  0.98 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 667.0 ns |   1.69 ns | 1.11 ns |  0.82 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 818.0 ns |   2.36 ns | 1.56 ns |  1.00 |         - |          NA |
|                    |            |                |             |          |           |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 763.0 ns |  13.36 ns | 0.73 ns |  0.93 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 665.4 ns |  36.39 ns | 1.99 ns |  0.81 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 818.9 ns | 179.98 ns | 9.87 ns |  1.00 |         - |          NA |
