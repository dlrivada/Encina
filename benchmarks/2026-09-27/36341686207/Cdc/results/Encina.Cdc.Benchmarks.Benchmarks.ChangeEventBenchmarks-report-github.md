```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error    | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|---------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 26.430 ns | 1.886 ns | 0.1034 ns |  0.80 | 0.0016 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.509 ns | 1.064 ns | 0.0583 ns |  0.05 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 13.516 ns | 3.114 ns | 0.1707 ns |  0.41 | 0.0007 |      56 B |        1.00 |
| CreateChangeMetadata       | 33.093 ns | 5.153 ns | 0.2825 ns |  1.00 | 0.0007 |      56 B |        1.00 |
