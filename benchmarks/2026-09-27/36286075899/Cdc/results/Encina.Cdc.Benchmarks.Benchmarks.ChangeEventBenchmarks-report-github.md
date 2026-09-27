```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 17.548 ns | 0.2189 ns | 0.3208 ns |  0.63 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.006 ns | 0.0201 ns | 0.0275 ns |  0.04 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 10.561 ns | 0.0834 ns | 0.1196 ns |  0.38 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 28.069 ns | 0.1099 ns | 0.1504 ns |  1.00 | 0.0033 |      56 B |        1.00 |
