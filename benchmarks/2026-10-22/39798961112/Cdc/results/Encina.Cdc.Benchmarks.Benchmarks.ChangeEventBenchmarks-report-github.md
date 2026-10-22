```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.58GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreateChangeEvent          | 22.926 ns | 0.8045 ns | 1.1793 ns |  0.70 |    0.05 | 0.0016 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.392 ns | 0.0348 ns | 0.0464 ns |  0.04 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 11.512 ns | 0.5119 ns | 0.7661 ns |  0.35 |    0.03 | 0.0007 |      56 B |        1.00 |
| CreateChangeMetadata       | 32.791 ns | 1.0157 ns | 1.5203 ns |  1.00 |    0.06 | 0.0007 |      56 B |        1.00 |
