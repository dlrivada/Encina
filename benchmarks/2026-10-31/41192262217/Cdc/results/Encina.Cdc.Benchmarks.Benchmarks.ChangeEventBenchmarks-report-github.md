```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error    | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|---------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 22.154 ns | 6.712 ns | 0.3679 ns |  0.63 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.419 ns | 1.025 ns | 0.0562 ns |  0.04 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 12.491 ns | 1.121 ns | 0.0614 ns |  0.35 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 35.434 ns | 1.587 ns | 0.0870 ns |  1.00 | 0.0033 |      56 B |        1.00 |
