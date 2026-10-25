```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 1,110.7 ns |   5.74 ns |  3.80 ns |  0.98 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     |   615.5 ns |   2.45 ns |  1.46 ns |  0.54 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 1,135.1 ns |   2.97 ns |  1.96 ns |  1.00 |         - |          NA |
|                    |            |                |             |            |           |          |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 1,197.3 ns | 113.20 ns |  6.20 ns |  1.05 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           |   618.7 ns | 190.00 ns | 10.41 ns |  0.54 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 1,135.8 ns |  32.12 ns |  1.76 ns |  1.00 |         - |          NA |
