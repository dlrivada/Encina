```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.53GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                  | Mean         | Error     | StdDev    | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|----------:|----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     18.64 ns |  0.072 ns |  0.103 ns |     1.29 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     14.41 ns |  0.066 ns |  0.099 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 15,193.17 ns | 57.522 ns | 82.496 ns | 1,054.58 |    9.07 | 0.3052 | 0.2899 |    5287 B |       47.21 |
| GenericTypeConstruction |     61.32 ns |  0.241 ns |  0.354 ns |     4.26 |    0.04 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     40.84 ns |  0.190 ns |  0.284 ns |     2.83 |    0.03 | 0.0105 |      - |     176 B |        1.57 |
