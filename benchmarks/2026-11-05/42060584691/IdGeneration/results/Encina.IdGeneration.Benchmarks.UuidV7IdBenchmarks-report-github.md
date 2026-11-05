```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error    | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|---------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 776.7 ns |  1.49 ns | 0.88 ns |  1.02 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 669.7 ns |  1.27 ns | 0.84 ns |  0.88 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 762.4 ns |  6.80 ns | 4.05 ns |  1.00 |         - |          NA |
|                    |            |                |             |          |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 798.5 ns | 23.80 ns | 1.30 ns |  1.03 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 672.1 ns |  6.52 ns | 0.36 ns |  0.87 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 774.9 ns | 46.33 ns | 2.54 ns |  1.00 |         - |          NA |
