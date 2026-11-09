```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.81GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 20.888 ns | 0.3870 ns | 0.0212 ns |  0.61 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  2.563 ns | 0.2981 ns | 0.0163 ns |  0.08 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 11.754 ns | 0.0904 ns | 0.0050 ns |  0.34 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 34.121 ns | 1.3816 ns | 0.0757 ns |  1.00 | 0.0033 |      56 B |        1.00 |
