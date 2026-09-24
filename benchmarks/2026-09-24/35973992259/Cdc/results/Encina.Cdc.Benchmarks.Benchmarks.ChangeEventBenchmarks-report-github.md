```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 22.702 ns | 2.9361 ns | 0.1609 ns |  0.63 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.213 ns | 0.0329 ns | 0.0018 ns |  0.03 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 12.996 ns | 1.4592 ns | 0.0800 ns |  0.36 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 36.086 ns | 1.2915 ns | 0.0708 ns |  1.00 | 0.0033 |      56 B |        1.00 |
