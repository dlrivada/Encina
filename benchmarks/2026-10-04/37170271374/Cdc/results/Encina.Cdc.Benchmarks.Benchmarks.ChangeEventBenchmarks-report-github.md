```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 20.825 ns | 0.0903 ns | 0.1296 ns |  0.60 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.388 ns | 0.0013 ns | 0.0019 ns |  0.04 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 12.114 ns | 0.2042 ns | 0.2993 ns |  0.35 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 34.849 ns | 0.1792 ns | 0.2683 ns |  1.00 | 0.0033 |      56 B |        1.00 |
