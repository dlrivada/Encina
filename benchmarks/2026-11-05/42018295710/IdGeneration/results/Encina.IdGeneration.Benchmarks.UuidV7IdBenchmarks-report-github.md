```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.93GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error    | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|---------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 763.8 ns |  4.48 ns | 2.96 ns |  0.99 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 670.0 ns |  0.77 ns | 0.40 ns |  0.87 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 771.3 ns |  1.34 ns | 0.79 ns |  1.00 |         - |          NA |
|                    |            |                |             |          |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 801.7 ns | 16.25 ns | 0.89 ns |  1.04 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 672.6 ns | 17.50 ns | 0.96 ns |  0.88 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 767.6 ns |  2.32 ns | 0.13 ns |  1.00 |         - |          NA |
