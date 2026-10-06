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
| CreateChangeEvent          | 25.204 ns | 4.6197 ns | 0.2532 ns |  0.80 | 0.0016 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.472 ns | 0.2743 ns | 0.0150 ns |  0.05 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 12.968 ns | 2.4189 ns | 0.1326 ns |  0.41 | 0.0007 |      56 B |        1.00 |
| CreateChangeMetadata       | 31.695 ns | 2.6527 ns | 0.1454 ns |  1.00 | 0.0007 |      56 B |        1.00 |
