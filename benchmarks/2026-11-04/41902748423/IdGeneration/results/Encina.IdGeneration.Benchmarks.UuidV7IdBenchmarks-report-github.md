```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 788.4 ns |   4.46 ns | 2.95 ns |  1.03 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 665.4 ns |   2.49 ns | 1.65 ns |  0.87 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 765.1 ns |   3.19 ns | 1.90 ns |  1.00 |         - |          NA |
|                    |            |                |             |          |           |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 782.3 ns |  46.63 ns | 2.56 ns |  0.98 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 668.7 ns | 151.40 ns | 8.30 ns |  0.84 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 799.0 ns |  21.88 ns | 1.20 ns |  1.00 |         - |          NA |
