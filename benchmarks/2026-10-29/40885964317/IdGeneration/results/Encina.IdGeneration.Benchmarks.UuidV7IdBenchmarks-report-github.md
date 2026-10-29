```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error    | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|---------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 904.6 ns |  5.48 ns | 3.26 ns |  1.00 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 585.4 ns |  1.61 ns | 0.84 ns |  0.65 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 902.1 ns |  2.21 ns | 1.46 ns |  1.00 |         - |          NA |
|                    |            |                |             |          |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 971.5 ns |  3.54 ns | 0.19 ns |  1.07 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 583.1 ns |  1.40 ns | 0.08 ns |  0.64 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 904.1 ns | 16.52 ns | 0.91 ns |  1.00 |         - |          NA |
