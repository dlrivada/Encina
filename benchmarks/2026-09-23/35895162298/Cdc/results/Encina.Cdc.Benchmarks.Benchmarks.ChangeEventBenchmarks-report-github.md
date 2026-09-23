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
| CreateChangeEvent          | 27.071 ns | 0.9271 ns | 0.0508 ns |  0.86 | 0.0016 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.445 ns | 0.3541 ns | 0.0194 ns |  0.05 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 13.529 ns | 1.2394 ns | 0.0679 ns |  0.43 | 0.0007 |      56 B |        1.00 |
| CreateChangeMetadata       | 31.550 ns | 1.6489 ns | 0.0904 ns |  1.00 | 0.0007 |      56 B |        1.00 |
