```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.65GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean       | Error    | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |-----------:|---------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 1,130.9 ns |  4.60 ns | 3.04 ns |  0.94 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     |   645.1 ns |  1.43 ns | 0.95 ns |  0.54 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 1,196.9 ns |  8.63 ns | 5.14 ns |  1.00 |         - |          NA |
|                    |            |                |             |            |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 1,140.2 ns | 20.21 ns | 1.11 ns |  1.01 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           |   661.1 ns |  2.37 ns | 0.13 ns |  0.58 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 1,132.8 ns | 45.13 ns | 2.47 ns |  1.00 |         - |          NA |
