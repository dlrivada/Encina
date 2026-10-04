```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method               | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error   | StdDev  | Ratio | Allocated | Alloc Ratio |
|--------------------- |----------- |--------------- |------------ |------------ |---------:|--------:|--------:|------:|----------:|------------:|
| Generate_WithShardId | Job-YFEFPZ | 10             | Default     | 3           | 242.1 ns | 0.10 ns | 0.06 ns |  1.00 |         - |          NA |
| GenerateAndGetValue  | Job-YFEFPZ | 10             | Default     | 3           | 242.1 ns | 0.08 ns | 0.05 ns |  1.00 |         - |          NA |
| Generate             | Job-YFEFPZ | 10             | Default     | 3           | 242.1 ns | 0.11 ns | 0.06 ns |  1.00 |         - |          NA |
| ExtractShardId       | Job-YFEFPZ | 10             | Default     | 3           | 240.4 ns | 0.08 ns | 0.05 ns |  0.99 |         - |          NA |
|                      |            |                |             |             |          |         |         |       |           |             |
| Generate_WithShardId | MediumRun  | 15             | 2           | 10          | 242.1 ns | 0.03 ns | 0.05 ns |  1.00 |         - |          NA |
| GenerateAndGetValue  | MediumRun  | 15             | 2           | 10          | 242.1 ns | 0.03 ns | 0.05 ns |  1.00 |         - |          NA |
| Generate             | MediumRun  | 15             | 2           | 10          | 242.1 ns | 0.02 ns | 0.04 ns |  1.00 |         - |          NA |
| ExtractShardId       | MediumRun  | 15             | 2           | 10          | 240.4 ns | 0.03 ns | 0.04 ns |  0.99 |         - |          NA |
