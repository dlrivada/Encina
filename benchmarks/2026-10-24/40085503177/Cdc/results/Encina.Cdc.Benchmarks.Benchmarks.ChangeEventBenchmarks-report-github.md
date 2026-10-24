```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 27.459 ns | 2.2304 ns | 0.1223 ns |  0.80 | 0.0016 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.763 ns | 0.8926 ns | 0.0489 ns |  0.05 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 14.549 ns | 1.8410 ns | 0.1009 ns |  0.42 | 0.0007 |      56 B |        1.00 |
| CreateChangeMetadata       | 34.363 ns | 2.4803 ns | 0.1360 ns |  1.00 | 0.0007 |      56 B |        1.00 |
