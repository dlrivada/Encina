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
| CreateChangeEvent          | 29.167 ns | 5.0852 ns | 0.2787 ns |  0.83 | 0.0016 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.668 ns | 0.6872 ns | 0.0377 ns |  0.05 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 14.988 ns | 0.4421 ns | 0.0242 ns |  0.43 | 0.0007 |      56 B |        1.00 |
| CreateChangeMetadata       | 35.098 ns | 0.8866 ns | 0.0486 ns |  1.00 | 0.0007 |      56 B |        1.00 |
