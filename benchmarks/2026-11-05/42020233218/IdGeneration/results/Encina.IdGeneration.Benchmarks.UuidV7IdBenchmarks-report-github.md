```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.64GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error    | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|---------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 655.3 ns |  4.78 ns | 3.16 ns |  1.00 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 686.5 ns |  5.63 ns | 2.95 ns |  1.05 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 655.4 ns |  6.29 ns | 3.75 ns |  1.00 |         - |          NA |
|                    |            |                |             |          |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 658.5 ns | 69.25 ns | 3.80 ns |  0.93 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 686.5 ns | 16.68 ns | 0.91 ns |  0.97 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 706.5 ns | 20.00 ns | 1.10 ns |  1.00 |         - |          NA |
