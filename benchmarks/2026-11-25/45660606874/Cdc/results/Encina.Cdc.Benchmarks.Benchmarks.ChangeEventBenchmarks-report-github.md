```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.86GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreateChangeEvent          | 23.835 ns |  1.1009 ns | 0.0603 ns |  0.64 |    0.02 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.250 ns |  0.4201 ns | 0.0230 ns |  0.03 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 13.200 ns |  1.1615 ns | 0.0637 ns |  0.35 |    0.01 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 37.249 ns | 21.5961 ns | 1.1838 ns |  1.00 |    0.04 | 0.0033 |      56 B |        1.00 |
