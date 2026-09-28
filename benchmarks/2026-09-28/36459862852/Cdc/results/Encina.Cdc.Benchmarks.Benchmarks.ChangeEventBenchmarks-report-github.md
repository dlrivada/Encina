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
| CreateChangeEvent          | 29.026 ns | 5.0381 ns | 0.2762 ns |  0.81 | 0.0016 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.684 ns | 0.4178 ns | 0.0229 ns |  0.05 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 14.570 ns | 1.2521 ns | 0.0686 ns |  0.41 | 0.0007 |      56 B |        1.00 |
| CreateChangeMetadata       | 35.717 ns | 3.8356 ns | 0.2102 ns |  1.00 | 0.0007 |      56 B |        1.00 |
