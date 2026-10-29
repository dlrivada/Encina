```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.86GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 23.345 ns | 3.1077 ns | 0.1703 ns |  0.65 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.236 ns | 0.2246 ns | 0.0123 ns |  0.03 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 13.242 ns | 3.0448 ns | 0.1669 ns |  0.37 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 35.828 ns | 1.1686 ns | 0.0641 ns |  1.00 | 0.0033 |      56 B |        1.00 |
