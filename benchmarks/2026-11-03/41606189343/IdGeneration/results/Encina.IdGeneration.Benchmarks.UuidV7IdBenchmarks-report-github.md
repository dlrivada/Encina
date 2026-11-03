```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.65GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 648.6 ns |   2.62 ns | 1.37 ns |  1.00 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 676.9 ns |   0.96 ns | 0.50 ns |  1.05 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 645.7 ns |   2.78 ns | 1.45 ns |  1.00 |         - |          NA |
|                    |            |                |             |          |           |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 651.1 ns |  10.91 ns | 0.60 ns |  0.94 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 681.6 ns | 121.94 ns | 6.68 ns |  0.99 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 689.7 ns | 114.17 ns | 6.26 ns |  1.00 |         - |          NA |
