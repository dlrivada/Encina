```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 17.939 ns | 1.6562 ns | 0.0908 ns |  0.66 | 0.0016 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.463 ns | 0.3861 ns | 0.0212 ns |  0.05 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 10.391 ns | 1.7009 ns | 0.0932 ns |  0.38 | 0.0007 |      56 B |        1.00 |
| CreateChangeMetadata       | 27.233 ns | 1.5116 ns | 0.0829 ns |  1.00 | 0.0007 |      56 B |        1.00 |
