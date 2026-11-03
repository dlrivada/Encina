```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.53GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev  | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|--------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 496.6 ns |   4.53 ns | 2.69 ns |  1.03 |    0.01 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 409.6 ns |   4.51 ns | 2.68 ns |  0.85 |    0.01 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 482.4 ns |  11.76 ns | 7.00 ns |  1.00 |    0.02 |         - |          NA |
|                    |            |                |             |          |           |         |       |         |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 475.4 ns |  83.79 ns | 4.59 ns |  1.00 |    0.01 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 411.0 ns |  31.26 ns | 1.71 ns |  0.86 |    0.01 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 476.0 ns | 109.20 ns | 5.99 ns |  1.00 |    0.02 |         - |          NA |
