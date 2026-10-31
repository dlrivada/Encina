```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.51GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error    | StdDev  | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|---------:|--------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 658.7 ns |  4.72 ns | 2.47 ns |  0.98 |    0.01 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 426.4 ns |  4.46 ns | 2.95 ns |  0.63 |    0.01 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 675.6 ns | 12.75 ns | 8.43 ns |  1.00 |    0.02 |         - |          NA |
|                    |            |                |             |          |          |         |       |         |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 654.0 ns | 29.04 ns | 1.59 ns |  0.95 |    0.01 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 430.7 ns | 72.67 ns | 3.98 ns |  0.63 |    0.01 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 688.9 ns | 86.00 ns | 4.71 ns |  1.00 |    0.01 |         - |          NA |
