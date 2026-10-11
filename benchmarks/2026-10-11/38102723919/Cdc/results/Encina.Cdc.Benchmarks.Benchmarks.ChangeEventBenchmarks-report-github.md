```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 23.963 ns | 0.2053 ns | 0.3073 ns |  0.66 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.386 ns | 0.0016 ns | 0.0022 ns |  0.04 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 13.227 ns | 0.1578 ns | 0.2362 ns |  0.36 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 36.448 ns | 0.1492 ns | 0.2233 ns |  1.00 | 0.0033 |      56 B |        1.00 |
