```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.64GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|---------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 770.0 ns |   4.58 ns |  2.73 ns |  1.00 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 670.9 ns |   2.27 ns |  1.35 ns |  0.87 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 768.9 ns |   6.05 ns |  3.60 ns |  1.00 |         - |          NA |
|                    |            |                |             |          |           |          |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 762.0 ns | 224.74 ns | 12.32 ns |  1.00 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 671.4 ns |   4.17 ns |  0.23 ns |  0.88 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 763.7 ns |  27.67 ns |  1.52 ns |  1.00 |         - |          NA |
