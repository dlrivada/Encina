```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error    | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|---------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 681.7 ns |  2.74 ns | 1.81 ns |  1.04 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 685.7 ns |  3.00 ns | 1.98 ns |  1.05 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 654.0 ns |  2.79 ns | 1.66 ns |  1.00 |         - |          NA |
|                    |            |                |             |          |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 684.1 ns | 10.60 ns | 0.58 ns |  1.04 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 686.0 ns |  3.85 ns | 0.21 ns |  1.04 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 658.5 ns | 20.18 ns | 1.11 ns |  1.00 |         - |          NA |
