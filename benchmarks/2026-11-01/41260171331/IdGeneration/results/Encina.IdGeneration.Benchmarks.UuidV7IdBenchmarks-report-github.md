```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.83GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3


```
| Method             | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error   | StdDev   | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |------------ |-----------:|--------:|---------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 3           | 1,169.1 ns | 1.82 ns |  1.08 ns |  0.99 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 3           |   751.7 ns | 1.06 ns |  0.70 ns |  0.63 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 3           | 1,184.3 ns | 5.35 ns |  3.19 ns |  1.00 |         - |          NA |
|                    |            |                |             |             |            |         |          |       |           |             |
| Generate_GetValue  | MediumRun  | 15             | 2           | 10          | 1,255.3 ns | 6.71 ns | 10.04 ns |  1.07 |         - |          NA |
| NewGuid_Comparison | MediumRun  | 15             | 2           | 10          |   756.2 ns | 3.23 ns |  4.73 ns |  0.65 |         - |          NA |
| Generate           | MediumRun  | 15             | 2           | 10          | 1,170.0 ns | 1.87 ns |  2.74 ns |  1.00 |         - |          NA |
