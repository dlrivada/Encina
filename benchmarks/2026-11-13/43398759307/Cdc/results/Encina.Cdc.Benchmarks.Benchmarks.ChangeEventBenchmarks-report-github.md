```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.86GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 23.575 ns | 4.2057 ns | 0.2305 ns |  0.65 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.241 ns | 0.3282 ns | 0.0180 ns |  0.03 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 13.233 ns | 4.1698 ns | 0.2286 ns |  0.37 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 36.122 ns | 2.3646 ns | 0.1296 ns |  1.00 | 0.0033 |      56 B |        1.00 |
