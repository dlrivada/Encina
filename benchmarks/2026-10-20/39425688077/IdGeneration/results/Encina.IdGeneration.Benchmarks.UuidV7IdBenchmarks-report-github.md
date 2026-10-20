```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     |   917.5 ns |   1.66 ns |  0.87 ns |  0.91 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     |   557.4 ns |   4.35 ns |  2.59 ns |  0.55 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 1,006.4 ns |   2.34 ns |  1.39 ns |  1.00 |         - |          NA |
|                    |            |                |             |            |           |          |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           |   974.8 ns |  55.84 ns |  3.06 ns |  1.05 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           |   566.3 ns | 104.56 ns |  5.73 ns |  0.61 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           |   928.2 ns | 198.21 ns | 10.86 ns |  1.00 |         - |          NA |
